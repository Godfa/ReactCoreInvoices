import { ErrorMessage, Form as FormikForm, Formik } from "formik";
import { observer } from "mobx-react-lite";
import { Button, Form, Label } from "semantic-ui-react";
import { useStore } from "../../app/stores/store";
import * as Yup from 'yup';
import { toast } from "react-toastify";

export default observer(function ChangePasswordForm() {
    const { userStore } = useStore();

    const validationSchema = Yup.object({
        newPassword: Yup.string().required('Uusi salasana vaaditaan').min(12, 'Salasanan on oltava vähintään 12 merkkiä'),
        confirmPassword: Yup.string()
            .required('Vahvista salasana')
            .oneOf([Yup.ref('newPassword')], 'Salasanojen on täsmättävä')
    });

    return (
        <div className="login-page">
            <div className="login-card">
                <div className="login-logo">M</div>
                <h1 className="login-title">Vaihda salasana</h1>

                <Formik
                    initialValues={{ newPassword: '', confirmPassword: '', error: null }}
                    validationSchema={validationSchema}
                    onSubmit={(values, { setErrors, resetForm }) => userStore.changePassword({
                        newPassword: values.newPassword
                    }).then(() => {
                        toast.success('Salasana vaihdettu onnistuneesti');
                        resetForm();
                        window.location.href = '/invoices';
                    }).catch(error =>
                        setErrors({ error: 'Salasanan vaihto epäonnistui.' }))}
                >
                    {({ handleSubmit, isSubmitting, errors, handleChange, values, touched }) => (
                        <FormikForm className='ui form login-form' onSubmit={handleSubmit} autoComplete='off'>
                            <Form.Input
                                name='newPassword'
                                placeholder='Uusi salasana'
                                type='password'
                                value={values.newPassword}
                                onChange={handleChange}
                                icon='lock'
                                iconPosition='left'
                                error={touched.newPassword && errors.newPassword}
                                fluid
                            />
                            <ErrorMessage name='newPassword' render={error => <Label style={{ marginBottom: 10 }} basic color='red' content={error} />} />

                            <Form.Input
                                name='confirmPassword'
                                placeholder='Vahvista uusi salasana'
                                type='password'
                                value={values.confirmPassword}
                                onChange={handleChange}
                                icon='lock'
                                iconPosition='left'
                                error={touched.confirmPassword && errors.confirmPassword}
                                fluid
                            />
                            <ErrorMessage name='confirmPassword' render={error => <Label style={{ marginBottom: 10 }} basic color='red' content={error} />} />

                            <ErrorMessage
                                name='error' render={() =>
                                    <Label style={{ marginBottom: 10, width: '100%', textAlign: 'center' }} basic color='red' content={errors.error} />}
                            />
                            <Button
                                loading={isSubmitting}
                                className="btn-primary"
                                content='Vaihda salasana'
                                type='submit'
                                fluid
                                size="large"
                            />
                        </FormikForm>
                    )}
                </Formik>
            </div>
        </div>
    )
})
